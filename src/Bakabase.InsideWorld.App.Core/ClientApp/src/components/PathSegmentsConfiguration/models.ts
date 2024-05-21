import type { ResourceProperty } from '@/sdk/constants';
import type { MatcherValue } from '@/components/PathSegmentsConfiguration/models/MatcherValue';

export type OnDeleteMatcherValue = (property: ResourceProperty, index: number) => void;

export type Value = { [type in ResourceProperty]?: MatcherValue[]; };
